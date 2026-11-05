using System;

namespace html;
/// <summary>Defines instance-based access to the HtmlByteDecoder operations.</summary>
public interface IHtmlByteDecoderApi
{
    /// <summary>Executes the Decode operation.</summary>
    public HtmlDecodedInput Decode(ReadOnlySpan<byte> input);
    /// <summary>Executes the DecodeWithLabel operation.</summary>
    public HtmlDecodedInput DecodeWithLabel(ReadOnlySpan<byte> input, string encodingLabel);
    /// <summary>Executes the DecodeWithDetection operation.</summary>
    public HtmlDecodedInput DecodeWithDetection(ReadOnlySpan<byte> input, HtmlEncodingDetectionResult detection);
}
