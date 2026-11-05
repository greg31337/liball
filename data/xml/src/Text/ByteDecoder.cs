using System;
using System.Text;

namespace xml;

internal class ByteDecoder
{
    internal IByteSource Source;
    internal Encoding SelectedEncoding;
    internal byte[] ReadBuffer;

    internal ByteDecoder(IByteSource source, Encoding encoding, int bufferSize)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(encoding, "encoding");
        Guard.Positive(bufferSize, "bufferSize");
        Source = source;
        SelectedEncoding = encoding;
        ReadBuffer = new byte[bufferSize];
    }

    internal string DecodeByteText()
    {
        ByteBuffer bytes = new ByteBuffer(ReadBuffer.Length);
        while (true)
        {
            int count = Source.ReadSourceBytes(ReadBuffer);
            if (count == 0)
            {
                break;
            }

            bytes.AppendBytes(new ReadOnlySpan<byte>(ReadBuffer, 0, count));
        }

        return SelectedEncoding.GetString(bytes.Bytes, 0, bytes.Count);
    }

    internal string InspectByteDecoderEncodingName()
    {
        return SelectedEncoding.WebName;
    }

    internal void CloseByteDecoder()
    {
        Source.CloseByteSource();
    }
}

internal static class ByteDecoderFactory
{
    internal static ByteDecoder CreateXmlInputByteDecoder(XmlInput input, EncodingPolicyKind policy, int bufferSize)
    {
        Encoding encoding = EncodingDetector.ResolveCallerSelectedEncoding(input, policy);
        IByteSource source = ByteSourceFactory.CreateXmlInputByteSource(input, bufferSize);
        return new ByteDecoder(source, encoding, bufferSize);
    }
}
