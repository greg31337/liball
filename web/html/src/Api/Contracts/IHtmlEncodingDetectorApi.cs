using System;

namespace html;
/// <summary>Defines instance-based access to the HtmlEncodingDetector operations.</summary>
public interface IHtmlEncodingDetectorApi
{
    /// <summary>Executes the GetEncodingName operation.</summary>
    public string GetEncodingName(string label);
    /// <summary>Executes the GetEncodingNameFromSpan operation.</summary>
    public string GetEncodingNameFromSpan(ReadOnlySpan<char> label);
    /// <summary>Executes the Detect operation.</summary>
    public HtmlEncodingDetectionResult Detect(ReadOnlySpan<byte> input);
    /// <summary>Executes the DetectWithTransportLabel operation.</summary>
    public HtmlEncodingDetectionResult DetectWithTransportLabel(ReadOnlySpan<byte> input, string transportLabel);
    /// <summary>Executes the DetectWithLabels operation.</summary>
    public HtmlEncodingDetectionResult DetectWithLabels(ReadOnlySpan<byte> input, string transportLabel, string defaultLabel);
}
