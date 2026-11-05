namespace uri.Tests;

public class TextOutputTests
{
    [Test]
    public void AppendOperationsAccumulateTextAndReportWrittenRanges()
    {
        TextOutput output = new TextOutput(20);

        TextOutputResult character = output.AppendCharacter('A');
        TextOutputResult text = output.AppendText("bc");
        TextOutputResult span = output.AppendSpan("de".AsSpan());
        TextOutputResult repeated = output.AppendRepeated('!', 3);

        Assert.Multiple(() =>
        {
            AssertSuccess(character, 0, 1);
            AssertSuccess(text, 1, 2);
            AssertSuccess(span, 3, 2);
            AssertSuccess(repeated, 5, 3);
            Assert.That(output.GetLength(), Is.EqualTo(8));
            Assert.That(output.GetText(), Is.EqualTo("Abcde!!!"));
            Assert.That(output.GetStatus().Success, Is.True);
        });
    }

    [Test]
    public void BufferGrowsWithoutChangingAccumulatedText()
    {
        TextOutput output = new TextOutput(200);
        string first = new string('a', 70);
        string second = new string('b', 90);

        TextOutputResult firstResult = output.AppendText(first);
        TextOutputResult secondResult = output.AppendText(second);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True);
            Assert.That(secondResult.Success, Is.True);
            Assert.That(output.GetLength(), Is.EqualTo(160));
            Assert.That(output.GetText(), Is.EqualTo(first + second));
        });
    }

    [Test]
    public void ExactLimitSucceedsAndOverflowIsAtomic()
    {
        TextOutput output = new TextOutput(5);

        TextOutputResult exact = output.AppendText("abcde");
        TextOutputResult overflow = output.AppendText("xy");

        Assert.Multiple(() =>
        {
            AssertSuccess(exact, 0, 5);
            AssertFailure(overflow, 5, 2);
            Assert.That(output.GetLength(), Is.EqualTo(5));
            Assert.That(output.GetText(), Is.EqualTo("abcde"));
        });
    }

    [Test]
    public void PartialCapacityReportsOnlyRequestedExcess()
    {
        TextOutput output = new TextOutput(6);
        output.AppendText("abcd");

        TextOutputResult overflow = output.AppendText("WXYZ");

        Assert.Multiple(() =>
        {
            AssertFailure(overflow, 6, 2);
            Assert.That(output.GetText(), Is.EqualTo("abcd"));
        });
    }

    [Test]
    public void FirstFailureIsStickyAcrossLaterAppends()
    {
        TextOutput output = new TextOutput(3);
        output.AppendText("ab");

        TextOutputResult firstFailure = output.AppendText("cd");
        TextOutputResult later = output.AppendCharacter('x');
        TextOutputResult status = output.GetStatus();

        Assert.Multiple(() =>
        {
            AssertFailure(firstFailure, 3, 1);
            AssertFailure(later, 3, 1);
            AssertFailure(status, 3, 1);
            Assert.That(output.GetText(), Is.EqualTo("ab"));
        });
    }

    [Test]
    public void ZeroLimitAcceptsEmptyAppendAndRejectsContent()
    {
        TextOutput output = new TextOutput(0);

        TextOutputResult empty = output.AppendSpan("".AsSpan());
        TextOutputResult content = output.AppendCharacter('x');

        Assert.Multiple(() =>
        {
            AssertSuccess(empty, 0, 0);
            AssertFailure(content, 0, 1);
            Assert.That(output.GetText(), Is.Empty);
        });
    }

    [Test]
    public void NegativeLimitStartsInDeterministicFailureState()
    {
        TextOutput output = new TextOutput(-1);

        TextOutputResult status = output.GetStatus();
        TextOutputResult append = output.AppendText("value");

        Assert.Multiple(() =>
        {
            AssertFailure(status, 0, 0);
            AssertFailure(append, 0, 0);
            Assert.That(output.GetText(), Is.Empty);
        });
    }

    [Test]
    public void NegativeRepeatCountFailsWithoutChangingOutput()
    {
        TextOutput output = new TextOutput(10);
        output.AppendText("ok");

        TextOutputResult result = output.AppendRepeated('x', -1);

        Assert.Multiple(() =>
        {
            AssertFailure(result, 2, 0);
            Assert.That(output.GetText(), Is.EqualTo("ok"));
        });
    }

    [Test]
    public void UnicodeOutputLimitCountsUtf16CodeUnits()
    {
        TextOutput output = new TextOutput(2);

        TextOutputResult emoji = output.AppendText("\ud83d\ude00");

        Assert.Multiple(() =>
        {
            AssertSuccess(emoji, 0, 2);
            Assert.That(output.GetLength(), Is.EqualTo(2));
            Assert.That(output.GetText(), Is.EqualTo("\ud83d\ude00"));
        });
    }

    [Test]
    public void MultipleGrowthStepsStillEnforceExactFinalLimit()
    {
        TextOutput output = new TextOutput(257);

        TextOutputResult first = output.AppendRepeated('a', 64);
        TextOutputResult second = output.AppendRepeated('b', 128);
        TextOutputResult exact = output.AppendRepeated('c', 65);
        TextOutputResult overflow = output.AppendCharacter('d');

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(exact.Success, Is.True);
            Assert.That(output.GetLength(), Is.EqualTo(257));
            AssertFailure(overflow, 257, 1);
        });
    }

    private static void AssertSuccess(TextOutputResult result, int offset, int length)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.WrittenOffset, Is.EqualTo(offset));
        Assert.That(result.WrittenLength, Is.EqualTo(length));
        Assert.That(result.ErrorOffset, Is.Zero);
        Assert.That(result.ErrorLength, Is.Zero);
    }

    private static void AssertFailure(TextOutputResult result, int offset, int length)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.WrittenLength, Is.Zero);
        Assert.That(result.ErrorOffset, Is.EqualTo(offset));
        Assert.That(result.ErrorLength, Is.EqualTo(length));
    }
}
