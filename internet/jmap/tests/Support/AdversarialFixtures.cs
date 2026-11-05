using System.Text;

namespace jmap.Tests;

internal static class AdversarialFixtures
{
    public static byte[] InvalidUtf8Json()
    {
        return new byte[] { (byte)'{', (byte)'\"', (byte)'x', (byte)'\"', (byte)':', (byte)'\"', 0xc3, 0x28, (byte)'\"', (byte)'}' };
    }

    public static byte[] DuplicateMemberJson()
    {
        return Encoding.UTF8.GetBytes("{\"using\":[],\"using\":[],\"methodCalls\":[]}");
    }

    public static byte[] NestedJson(int depth)
    {
        StringBuilder json = new StringBuilder(depth * 2 + 1);
        for (int index = 0; index < depth; index++)
        {
            json.Append('[');
        }

        json.Append('0');
        for (int index = 0; index < depth; index++)
        {
            json.Append(']');
        }

        return Encoding.UTF8.GetBytes(json.ToString());
    }

    public static byte[] OversizedJsonString(int utf8ByteCount)
    {
        return Encoding.UTF8.GetBytes("{\"value\":\"" + new string('x', utf8ByteCount) + "\"}");
    }

    public static byte[] MissingMultipartTerminator()
    {
        return Encoding.ASCII.GetBytes(
            "MIME-Version: 1.0\r\n" +
            "Content-Type: multipart/mixed; boundary=unfinished\r\n\r\n" +
            "--unfinished\r\nContent-Type: text/plain\r\n\r\nbody\r\n");
    }

    public static byte[] OversizedMimeHeader(int valueLength)
    {
        return Encoding.ASCII.GetBytes(
            "X-Adversarial: " + new string('x', valueLength) + "\r\n" +
            "Content-Type: text/plain\r\n\r\nbody");
    }

    public static byte[] NestedMessageRfc822(int depth)
    {
        StringBuilder message = new StringBuilder();
        for (int index = 0; index < depth; index++)
        {
            message.Append("Content-Type: message/rfc822\r\n\r\n");
        }

        message.Append("Content-Type: text/plain\r\n\r\nleaf");
        return Encoding.ASCII.GetBytes(message.ToString());
    }
}
