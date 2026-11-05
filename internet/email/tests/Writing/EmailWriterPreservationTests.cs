using System.Text;
using email;

namespace email.Tests;

public class EmailWriterPreservationTests
{
    [Test]
    public void ExactModeCopiesAnUntouchedMessageByteForByte()
    {
        byte[] source = Bytes("sUbJeCt: exact\nX-Fold: one\r\n\ttwo\n\nbody\n");
        EmailMessage message = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(source, new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessageAndOptions(message!, new EmailWriteOptionsOperations().CreateExactPreservation());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(result.GetBytes(), Is.EqualTo(source));
        Assert.That(result.GetSourceByteCount(), Is.EqualTo(source.Length));
        Assert.That(result.GetCopiedByteCount(), Is.EqualTo(source.Length));
        Assert.That(result.GetRegeneratedByteCount(), Is.Zero);
        Assert.That(result.GetCopiedRangeCount(), Is.EqualTo(1));
        Assert.That(result.GetRegeneratedRangeCount(), Is.Zero);
    }

    [Test]
    public void ExactModeRejectsDirtyOrSourceDetachedMessagesWithoutPartialOutput()
    {
        EmailMessage dirty = Parse("Subject: old\r\n\r\nbody");
        dirty.GetHeaders()!.GetFirst("Subject")!.SetValue(" changed");
        EmailMessage detached = Parse("Subject: detached\r\n\r\nbody");
        detached.DetachSource(100);

        EmailWriteResult dirtyResult = new EmailWriter().WriteMessageWithMessageAndOptions(dirty, new EmailWriteOptionsOperations().CreateExactPreservation());
        EmailWriteResult detachedResult = new EmailWriter().WriteMessageWithMessageAndOptions(detached, new EmailWriteOptionsOperations().CreateExactPreservation());

        AssertRejected(dirtyResult);
        AssertRejected(detachedResult);
    }

    [Test]
    public void MinimalModeRegeneratesDirtyHeadersAndCopiesTheBodyExactly()
    {
        const string source = "sUbJeCt: old\nX-Keep: exact\r\n\r\nbody\nwith original ending";
        EmailMessage message = Parse(source);
        message.GetHeaders()!.GetFirst("Subject")!.SetValue(" changed");

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessageAndOptions(message, new EmailWriteOptionsOperations().CreateMinimalRegeneration());

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Text(result.GetBytes()), Is.EqualTo("Subject: changed\r\nX-Keep: exact\r\n\r\nbody\nwith original ending"));
        Assert.That(result.GetCopiedByteCount(), Is.GreaterThan(0));
        Assert.That(result.GetRegeneratedByteCount(), Is.EqualTo(Bytes("Subject: changed\r\n").Length));
        Assert.That(result.GetRegeneratedRangeCount(), Is.EqualTo(1));
    }

    [Test]
    public void MinimalModeReplacesARootLeafBodyWithoutTouchingHeaders()
    {
        const string source = "Subject: keep\nContent-Type: text/plain\n\nold body";
        EmailMessage message = Parse(source);
        message.GetBody()!.ReplaceRawContent(Content("new body"));

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessage(message);

        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Text(result.GetBytes()), Is.EqualTo("Subject: keep\nContent-Type: text/plain\n\nnew body"));
        Assert.That(result.GetRegeneratedByteCount(), Is.EqualTo(8));
        Assert.That(result.GetRegeneratedRangeCount(), Is.EqualTo(1));
    }

    [Test]
    public void MinimalModeSplicesOneDirtyChildWhileCopyingMultipartFramingAndSiblings()
    {
        const string source = "Content-Type: multipart/mixed; boundary=b\r\n\r\npreamble\r\n--b\r\nX-Part: one\r\n\r\nfirst\r\n--b\r\nX-Part: two\r\n\r\nsecond\r\n--b--\r\nepilogue";
        EmailMessage message = Parse(source);
        message.GetBody()!.GetChildAt(0).ReplaceRawContent(Content("changed"));

        EmailWriteResult result = new EmailWriter().WriteMessageWithMessage(message);

        const string expected = "Content-Type: multipart/mixed; boundary=b\r\n\r\npreamble\r\n--b\r\nX-Part: one\r\n\r\nchanged\r\n--b\r\nX-Part: two\r\n\r\nsecond\r\n--b--\r\nepilogue";
        Assert.That(result.IsSuccess(), Is.True);
        Assert.That(Text(result.GetBytes()), Is.EqualTo(expected));
        Assert.That(result.GetRegeneratedRangeCount(), Is.EqualTo(1));
        Assert.That(result.GetCopiedRangeCount(), Is.EqualTo(2));
        Assert.That(Text(result.GetBytes()), Does.Contain("X-Part: two\r\n\r\nsecond"));
    }

    [Test]
    public void MinimalModeReturnsNoPartialOutputForUnsupportedStructuralChangesOrLimits()
    {
        EmailMessage structural = Parse("Content-Type: multipart/mixed; boundary=b\r\n\r\n--b\r\n\r\none\r\n--b--\r\n");
        structural.GetBody()!.MarkDirty();
        EmailWriteOptions limitedOptions = new EmailWriteOptionsOperations().CreateMinimalRegeneration();
        limitedOptions.SetMaximumOutputBytes(5);

        EmailWriteResult structuralResult = new EmailWriter().WriteMessageWithMessage(structural);
        EmailWriteResult limitedResult = new EmailWriter().WriteMessageWithMessageAndOptions(Parse("\r\nlong body"), limitedOptions);

        AssertRejected(structuralResult);
        AssertRejected(limitedResult);
    }

    static void AssertRejected(EmailWriteResult result)
    {
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.HasFatalError(), Is.True);
        Assert.That(result.GetByteCount(), Is.Zero);
        Assert.That(result.GetIssues().ContainsCode(EmailIssueCode.WritePolicyViolation), Is.True);
    }

    static EmailMessage Parse(string value)
    {
        return new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Bytes(value), new EmailParseOptionsOperations().CreatePreserving()).GetMessage()!;
    }

    static EmailContent Content(string value)
    {
        return new EmailContentOperations().CreateMemory(Bytes(value), EmailContentOwnership.Owned, false);
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
