using System;

namespace html;
/// <summary>Provides instance-based access to the HtmlEncodingDetector operations.</summary>
public class HtmlEncodingDetectorApi : IHtmlEncodingDetectorApi
{
    /// <summary>Executes the GetEncodingName operation.</summary>
    public string GetEncodingName(string label)
    {
        return HtmlEncodingDetector.GetEncodingName(label);
    }

    /// <summary>Executes the GetEncodingNameFromSpan operation.</summary>
    public string GetEncodingNameFromSpan(ReadOnlySpan<char> label)
    {
        return HtmlEncodingDetector.GetEncodingName(label);
    }

    /// <summary>Executes the Detect operation.</summary>
    public HtmlEncodingDetectionResult Detect(ReadOnlySpan<byte> input)
    {
        return HtmlEncodingDetector.Detect(input);
    }

    /// <summary>Executes the DetectWithTransportLabel operation.</summary>
    public HtmlEncodingDetectionResult DetectWithTransportLabel(ReadOnlySpan<byte> input, string transportLabel)
    {
        return HtmlEncodingDetector.Detect(input, transportLabel);
    }

    /// <summary>Executes the DetectWithLabels operation.</summary>
    public HtmlEncodingDetectionResult DetectWithLabels(ReadOnlySpan<byte> input, string transportLabel, string defaultLabel)
    {
        return HtmlEncodingDetector.Detect(input, transportLabel, defaultLabel);
    }
}
