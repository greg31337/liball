using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Provides preservation-first ActiveX control inventory, replacement, and security operations.
/// </summary>
public class PptxActiveXControlOperations : IPptxActiveXControlOperations
{
    /// <summary>
    /// Creates an ActiveX control operations instance.
    /// </summary>
    public PptxActiveXControlOperations()
    {
    }

    /// <summary>
    /// Returns all ActiveX controls in presentation and slide order.
    /// </summary>
    /// <param name="presentation">The presentation to inspect.</param>
    /// <returns>The ActiveX control inventory.</returns>
    public List<PptxActiveXControl> GetInventory(PptxPresentation presentation)
    {
        return presentation.GetActiveXControls();
    }

    /// <summary>
    /// Returns all ActiveX controls on one slide.
    /// </summary>
    /// <param name="slide">The slide to inspect.</param>
    /// <returns>The slide ActiveX control inventory.</returns>
    public List<PptxActiveXControl> GetSlideInventory(PptxSlide slide)
    {
        return slide.GetActiveXControls();
    }

    /// <summary>
    /// Returns the complete persisted control XML.
    /// </summary>
    /// <param name="target">The ActiveX control to inspect.</param>
    /// <returns>The persisted control XML.</returns>
    public string GetControlXml(PptxActiveXControl target)
    {
        return target.GetControlXml();
    }

    /// <summary>
    /// Returns the binary control-property payload, or an empty array for an inline property bag.
    /// </summary>
    /// <param name="target">The ActiveX control to inspect.</param>
    /// <returns>The binary control-property bytes.</returns>
    public byte[] GetPropertyBytes(PptxActiveXControl target)
    {
        return target.GetPropertyBytes();
    }

    /// <summary>
    /// Replaces the complete control XML while preserving its package relationships.
    /// </summary>
    /// <param name="target">The ActiveX control to modify.</param>
    /// <param name="xml">The complete <c>ax:ocx</c> XML.</param>
    public void ReplaceControlXml(PptxActiveXControl target, string xml)
    {
        target.ReplaceControlXml(xml);
    }

    /// <summary>
    /// Replaces or creates the binary control-property persistence payload.
    /// </summary>
    /// <param name="target">The ActiveX control to modify.</param>
    /// <param name="bytes">The persisted property bytes.</param>
    /// <param name="contentType">The property part content type.</param>
    /// <param name="persistence">The ActiveX persistence mode.</param>
    public void ReplacePropertyBytes(PptxActiveXControl target, byte[] bytes, string contentType, string persistence)
    {
        target.ReplacePropertyBytes(bytes, contentType, persistence);
    }

    /// <summary>
    /// Replaces binary persistence with an inline ActiveX property bag.
    /// </summary>
    /// <param name="target">The ActiveX control to modify.</param>
    /// <param name="properties">The replacement properties in persistence order.</param>
    public void ReplaceProperties(PptxActiveXControl target, List<PptxActiveXProperty> properties)
    {
        target.ReplaceProperties(properties);
    }

    /// <summary>
    /// Scans the control relationship, persistence part, and property data for unsafe content.
    /// </summary>
    /// <param name="target">The ActiveX control to inspect.</param>
    /// <returns>The security diagnostics.</returns>
    public PptxResult ScanSecurity(PptxActiveXControl target)
    {
        return target.ScanSecurity();
    }
}
