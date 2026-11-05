using argon2;

namespace argon2.Tests.Text;

public class Utf8PasswordEncodingTests
{
    [TestCase("", "")]
    [TestCase("password", "70617373776F7264")]
    [TestCase("A\0é😀", "4100C3A9F09F9880")]
    public void EncodesExactUtf8Bytes(string text, string expectedHex)
    {
        using (SecureBuffer buffer = Utf8PasswordEncoding.Encode(text))
        {
            Assert.That(Convert.ToHexString(buffer.GetSpan()), Is.EqualTo(expectedHex));
        }
    }

    [Test]
    public void DoesNotNormalizeUnicodeText()
    {
        string composedText = "é";
        string decomposedText = "e\u0301";

        using (SecureBuffer composed = Utf8PasswordEncoding.Encode(composedText))
        using (SecureBuffer decomposed = Utf8PasswordEncoding.Encode(decomposedText))
        {
            Assert.Multiple(() =>
            {
                Assert.That(Convert.ToHexString(composed.GetSpan()), Is.EqualTo("C3A9"));
                Assert.That(Convert.ToHexString(decomposed.GetSpan()), Is.EqualTo("65CC81"));
                Assert.That(composed.GetSpan().ToArray(), Is.Not.EqualTo(decomposed.GetSpan().ToArray()));
                Assert.That(composedText, Is.EqualTo("é"));
                Assert.That(decomposedText, Is.EqualTo("e\u0301"));
            });
        }
    }

    [Test]
    public void EncodedBytesAreClearedWhenDisposed()
    {
        SecureBuffer buffer = Utf8PasswordEncoding.Encode("clear me");
        Assert.That(buffer.IsCleared(), Is.False);

        buffer.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(buffer.IsDisposed(), Is.True);
            Assert.That(buffer.IsCleared(), Is.True);
        });
    }

    [Test]
    public void EncodedBytesAreClearedOnExceptionalUsingExit()
    {
        SecureBuffer buffer = Utf8PasswordEncoding.Encode("clear me after failure");
        byte[] encodedBytes = buffer.GetArray();

        Assert.Throws<InvalidOperationException>(() => ThrowAfterEncoding(buffer));

        Assert.Multiple(() =>
        {
            Assert.That(buffer.IsDisposed(), Is.True);
            Assert.That(buffer.IsCleared(), Is.True);
            Assert.That(encodedBytes, Is.All.Zero);
        });
    }

    [Test]
    public void NullTextIsRejectedAsAnInvalidCallerInput()
    {
        Assert.Throws<Argon2ParameterException>(() => Utf8PasswordEncoding.Encode(null!));
    }

    static void ThrowAfterEncoding(SecureBuffer buffer)
    {
        using (buffer)
        {
            throw new InvalidOperationException("Expected test failure.");
        }
    }
}
