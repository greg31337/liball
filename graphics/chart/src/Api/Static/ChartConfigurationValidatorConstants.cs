namespace chart;
/// <summary>Contains stable issue codes produced by configuration validation.</summary>
public static class ChartConfigurationValidatorConstants
{
    /// <summary>A required object or nested value is missing.</summary>
    public const string MissingRequiredValueCode = "MissingRequiredValue";
    /// <summary>Data shape or channel content is invalid.</summary>
    public const string InvalidDataCode = "InvalidData";
    /// <summary>A scale or scale policy is invalid.</summary>
    public const string InvalidScaleCode = "InvalidScale";
    /// <summary>An axis configuration is invalid.</summary>
    public const string InvalidAxisCode = "InvalidAxis";
    /// <summary>A grid, layout result, or layout item is invalid.</summary>
    public const string InvalidLayoutCode = "InvalidLayout";
    /// <summary>A chart or retained render style is invalid.</summary>
    public const string InvalidStyleCode = "InvalidStyle";
    /// <summary>A retained render command is invalid.</summary>
    public const string InvalidRenderCommandCode = "InvalidRenderCommand";
    /// <summary>A bitmap, PNG, or SVG encoder option is invalid.</summary>
    public const string InvalidEncoderOptionCode = "InvalidEncoderOption";
    /// <summary>Accessibility descriptions or sidecar metadata are invalid.</summary>
    public const string InvalidAccessibilityCode = "InvalidAccessibility";
    /// <summary>A unified export option is invalid.</summary>
    public const string InvalidExportOptionCode = "InvalidExportOption";
    /// <summary>A timeline or frame configuration is invalid.</summary>
    public const string InvalidTimelineCode = "InvalidTimeline";
    /// <summary>An identifier is duplicated within its owning collection.</summary>
    public const string DuplicateIdentifierCode = "DuplicateIdentifier";
}
