using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffAsciiTests
{
    [Test]
    public void EncodeString_AppendsNulTerminator()
    {
        Assert.That(TiffAscii.EncodeString("hello"), Is.EqualTo(new byte[] { 104, 101, 108, 108, 111, 0 }));
        Assert.That(TiffAscii.EncodeString(null!), Is.EqualTo(new byte[] { 0 }));
    }

    [Test]
    public void EncodeStrings_NulSeparatesAndTerminatesEachString()
    {
        byte[] bytes = TiffAscii.EncodeStrings(new string[] { "one", "two", "" });

        Assert.That(bytes, Is.EqualTo(new byte[] { 111, 110, 101, 0, 116, 119, 111, 0, 0 }));
        Assert.That(TiffAscii.CalculateEncodedLength(new string[] { "one", "two", "" }), Is.EqualTo(9));
    }

    [Test]
    public void EncodeStrings_ReplacesNonAsciiCharactersWithQuestionMark()
    {
        byte[] bytes = TiffAscii.EncodeString("caf\u00E9");

        Assert.That(bytes, Is.EqualTo(new byte[] { 99, 97, 102, 63, 0 }));
    }

    [Test]
    public void Decode_ReadsSingleNulTerminatedString()
    {
        TiffAsciiResult result = TiffAscii.Decode(new byte[] { 104, 101, 108, 108, 111, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Text, Is.EqualTo("hello"));
        Assert.That(result.Values, Is.EqualTo(new string[] { "hello" }));
    }

    [Test]
    public void Decode_ReadsMultipleNulTerminatedStrings()
    {
        TiffAsciiResult result = TiffAscii.Decode(new byte[] { 111, 110, 101, 0, 116, 119, 111, 0, 0 });

        Assert.That(result.Success, Is.True);
        Assert.That(result.Values, Is.EqualTo(new string[] { "one", "two", "" }));
        Assert.That(result.Text, Is.EqualTo("one\0two\0"));
        Assert.That(TiffAscii.CountStrings(new byte[] { 111, 110, 101, 0, 116, 119, 111, 0, 0 }), Is.EqualTo(3));
    }

    [Test]
    public void Decode_ReportsMissingNulTerminator()
    {
        TiffAsciiResult result = TiffAscii.Decode(new byte[] { 104, 105 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidAsciiValue), Is.True);
        Assert.That(result.Values, Is.EqualTo(new string[] { "hi" }));
    }

    [Test]
    public void Decode_ReportsBytesOutsideAsciiRange()
    {
        TiffAsciiResult result = TiffAscii.Decode(new byte[] { 99, 97, 102, 233, 0 });

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidAsciiValue), Is.True);
        Assert.That(result.Values, Is.EqualTo(new string[] { "caf?" }));
    }

    [Test]
    public void Decode_ReportsEmptyByteArrayAsInvalid()
    {
        TiffAsciiResult result = TiffAscii.Decode(new byte[0]);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.ContainsCode(TiffErrorCode.InvalidAsciiValue), Is.True);
        Assert.That(result.Values.Length, Is.EqualTo(0));
    }

    [Test]
    public void EnsureNulTerminated_AppendsOnlyWhenNeeded()
    {
        Assert.That(TiffAscii.IsNulTerminated(new byte[] { 1, 0 }), Is.True);
        Assert.That(TiffAscii.IsNulTerminated(new byte[] { 1 }), Is.False);
        Assert.That(TiffAscii.EnsureNulTerminated(new byte[] { 1, 0 }), Is.EqualTo(new byte[] { 1, 0 }));
        Assert.That(TiffAscii.EnsureNulTerminated(new byte[] { 1 }), Is.EqualTo(new byte[] { 1, 0 }));
        Assert.That(TiffAscii.EnsureNulTerminated(null!), Is.EqualTo(new byte[] { 0 }));
    }

    [Test]
    public void TagValue_CreateAsciiArrayStoresValuesAndEncodedCount()
    {
        string[] source = new string[] { "one", "two" };

        TiffTagValue value = TiffTagValue.CreateAsciiArray(270, source);
        source[0] = "changed";

        Assert.That(value.IsAscii(), Is.True);
        Assert.That(value.TextValues, Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(value.Text, Is.EqualTo("one\0two"));
        Assert.That(value.Count, Is.EqualTo(8));
        Assert.That(value.GetValueByteCount(), Is.EqualTo(8));
    }

    [Test]
    public void TagValue_CopyDeepCopiesAsciiValues()
    {
        TiffTagValue value = TiffTagValue.CreateAsciiArray(270, new string[] { "one", "two" });

        TiffTagValue copy = value.Copy();
        value.TextValues[0] = "changed";

        Assert.That(copy.TextValues, Is.EqualTo(new string[] { "one", "two" }));
        Assert.That(copy.Text, Is.EqualTo("one\0two"));
    }
}
