using whois;

namespace whois.Tests;

public class LineScannerTests
{
    [TestCase("\r\n")]
    [TestCase("\n")]
    [TestCase("\r")]
    public void RecognizesEachNewlineForm(string newline)
    {
        string text = "alpha" + newline + "beta";

        string[] lines = LineScanner.ScanLines(text);

        Assert.That(lines, Is.EqualTo(new[] { "alpha", "beta" }));
        Assert.That(LineScanner.CountLines(text), Is.EqualTo(lines.Length));
    }

    [Test]
    public void RecognizesMixedNewlineFormsWithoutCrlfDoubleCounting()
    {
        string text = "one\r\ntwo\nthree\rfour";

        string[] lines = LineScanner.ScanLines(text);

        Assert.That(lines, Is.EqualTo(new[] { "one", "two", "three", "four" }));
        Assert.That(LineScanner.CountLines(text), Is.EqualTo(4));
    }

    [Test]
    public void PreservesLeadingAndTrailingBlankLines()
    {
        string text = "\r\nalpha\n\n";

        string[] lines = LineScanner.ScanLines(text);

        Assert.That(lines, Is.EqualTo(new[] { string.Empty, "alpha", string.Empty }));
        Assert.That(LineScanner.CountLines(text), Is.EqualTo(3));
    }

    [TestCase("\r\n", 1)]
    [TestCase("\n\n", 2)]
    [TestCase("\r\r\n", 2)]
    public void NewlineOnlyTextProducesTerminatedBlankLines(string text, int expectedCount)
    {
        string[] lines = LineScanner.ScanLines(text);

        Assert.That(lines, Has.Length.EqualTo(expectedCount));
        Assert.That(lines, Is.All.Empty);
        Assert.That(LineScanner.CountLines(text), Is.EqualTo(expectedCount));
    }

    [Test]
    public void RetainsUnterminatedFinalLineExactly()
    {
        string text = " name: value \r\nfinal\0value ";

        string[] lines = LineScanner.ScanLines(text);

        Assert.That(lines, Is.EqualTo(new[] { " name: value ", "final\0value " }));
        Assert.That(LineScanner.CountLines(text), Is.EqualTo(2));
    }

    [Test]
    public void EmptyTextContainsNoDerivedLines()
    {
        Assert.That(LineScanner.ScanLines(string.Empty), Is.Empty);
        Assert.That(LineScanner.CountLines(string.Empty), Is.Zero);
    }

    [Test]
    public void ScanningDoesNotReplaceTheDecodedSourceText()
    {
        string original = "first\rsecond\n";
        WhoisTextResponse response = new WhoisTextResponse();
        response.Text = original;

        string[] lines = LineScanner.ScanLines(response.Text);

        Assert.That(lines, Is.EqualTo(new[] { "first", "second" }));
        Assert.That(response.Text, Is.SameAs(original));
        Assert.That(response.Text, Is.EqualTo("first\rsecond\n"));
    }

    [Test]
    public void RejectsNullText()
    {
        Assert.That(() => LineScanner.ScanLines(null!), Throws.TypeOf<ArgumentNullException>());
        Assert.That(() => LineScanner.CountLines(null!), Throws.TypeOf<ArgumentNullException>());
    }
}
