using System;
using System.Text;

namespace argon2;

internal static class Utf8PasswordEncoding
{
    internal static SecureBuffer Encode(string text)
    {
        if (text == null)
        {
            throw new Argon2ParameterException("Password text is required.");
        }

        int byteCount = Encoding.UTF8.GetByteCount(text);
        SecureBuffer buffer = new SecureBuffer(byteCount);
        Encoding.UTF8.GetBytes(text.AsSpan(), buffer.GetSpan());
        return buffer;
    }
}
