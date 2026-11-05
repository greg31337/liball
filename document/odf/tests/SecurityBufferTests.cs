using System.Text;

namespace odf.Tests;

public class SecurityBufferTests
{
    [Test]
    public void OwnsInputAndSeparatesCallerCopiesFromOperationBytes()
    {
        byte[] source = [1, 2, 3, 4];
        SecurityBuffer buffer = new SecurityBuffer(source);
        source[0] = 99;
        byte[] operationBytes = buffer.GetOperationBytes();
        byte[] callerCopy = buffer.CopyBytes();
        callerCopy[1] = 88;

        Assert.Multiple(() =>
        {
            Assert.That(buffer.GetLength(), Is.EqualTo(4));
            Assert.That(operationBytes, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(buffer.GetOperationBytes()[1], Is.EqualTo(2));
            Assert.That(source[0], Is.EqualTo(99));
        });

        buffer.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(AllBytesAreZero(operationBytes), Is.True);
            Assert.That(buffer.IsCleared(), Is.True);
            Assert.That(callerCopy[1], Is.EqualTo(88));
        });
        SecurityBuffer.ClearBytes(callerCopy);
    }

    [Test]
    public void CreatesUtf8PasswordBytesAndClearsCallerOwnedArraysExplicitly()
    {
        string password = "pässword-🔒";
        byte[] expected = Encoding.UTF8.GetBytes(password);
        SecurityBuffer buffer = SecurityBuffer.CreateUtf8(password);
        byte[] operationBytes = buffer.GetOperationBytes();
        byte[] temporary = [9, 8, 7, 6];

        SecurityBuffer.ClearBytes(temporary);

        Assert.Multiple(() =>
        {
            Assert.That(operationBytes, Is.EqualTo(expected));
            Assert.That(AllBytesAreZero(temporary), Is.True);
        });

        buffer.Clear();
        Assert.That(AllBytesAreZero(operationBytes), Is.True);
        SecurityBuffer.ClearBytes(expected);
    }

    [Test]
    public void UsingScopeClearsSecretBytesWhenAnOperationThrows()
    {
        byte[] observedBytes = [];

        Assert.Throws<InvalidOperationException>(() =>
        {
            using SecurityBuffer buffer = new SecurityBuffer([5, 4, 3, 2, 1]);
            observedBytes = buffer.GetOperationBytes();
            throw new InvalidOperationException("operation failed");
        });

        Assert.That(AllBytesAreZero(observedBytes), Is.True);
    }

    [Test]
    public void ClearingIsIdempotentAndRejectsLaterSecretAccess()
    {
        SecurityBuffer buffer = new SecurityBuffer([1]);

        buffer.Dispose();
        buffer.Dispose();
        buffer.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(buffer.IsCleared(), Is.True);
            Assert.Throws<ObjectDisposedException>(() => buffer.GetOperationBytes());
            Assert.Throws<ObjectDisposedException>(() => buffer.CopyBytes());
            Assert.Throws<ObjectDisposedException>(() => buffer.GetLength());
        });
    }

    static bool AllBytesAreZero(byte[] data)
    {
        foreach (byte value in data)
        {
            if (value != 0)
            {
                return false;
            }
        }
        return true;
    }
}
