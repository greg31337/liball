using System;

namespace html;
/// <summary>Provides instance-based access to the HtmlByteDecoder operations.</summary>
public class HtmlByteDecoderApi : IHtmlByteDecoderApi
{
    /// <summary>Executes the Decode operation.</summary>
    public HtmlDecodedInput Decode(ReadOnlySpan<byte> input)
    {
        return HtmlByteDecoder.Decode(input);
    }

    /// <summary>Executes the DecodeWithLabel operation.</summary>
    public HtmlDecodedInput DecodeWithLabel(ReadOnlySpan<byte> input, string encodingLabel)
    {
        return HtmlByteDecoder.Decode(input, encodingLabel);
    }

    /// <summary>Executes the DecodeWithDetection operation.</summary>
    public HtmlDecodedInput DecodeWithDetection(ReadOnlySpan<byte> input, HtmlEncodingDetectionResult detection)
    {
        return HtmlByteDecoder.Decode(input, detection);
    }
}
