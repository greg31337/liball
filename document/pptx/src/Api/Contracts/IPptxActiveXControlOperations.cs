using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Defines preservation-first ActiveX control inventory, replacement, and security operations.
/// </summary>
public interface IPptxActiveXControlOperations
{
    /// <summary>
    /// Returns all ActiveX controls in presentation and slide order.
    /// </summary>
    /// <param name="presentation">The presentation to inspect.</param>
    /// <returns>The ActiveX control inventory.</returns>
    List<PptxActiveXControl> GetInventory(PptxPresentation presentation);
    /// <summary>
    /// Returns all ActiveX controls on one slide.
    /// </summary>
    /// <param name="slide">The slide to inspect.</param>
    /// <returns>The slide ActiveX control inventory.</returns>
    List<PptxActiveXControl> GetSlideInventory(PptxSlide slide);
    /// <summary>
    /// Returns the complete persisted control XML.
    /// </summary>
    /// <param name="target">The ActiveX control to inspect.</param>
    /// <returns>The persisted control XML.</returns>
    string GetControlXml(PptxActiveXControl target);
    /// <summary>
    /// Returns the binary control-property payload, or an empty array for an inline property bag.
    /// </summary>
    /// <param name="target">The ActiveX control to inspect.</param>
    /// <returns>The binary control-property bytes.</returns>
    byte[] GetPropertyBytes(PptxActiveXControl target);
    /// <summary>
    /// Replaces the complete control XML while preserving its package relationships.
    /// </summary>
    /// <param name="target">The ActiveX control to modify.</param>
    /// <param name="xml">The complete <c>ax:ocx</c> XML.</param>
    void ReplaceControlXml(PptxActiveXControl target, string xml);
    /// <summary>
    /// Replaces or creates the binary control-property persistence payload.
    /// </summary>
    /// <param name="target">The ActiveX control to modify.</param>
    /// <param name="bytes">The persisted property bytes.</param>
    /// <param name="contentType">The property part content type.</param>
    /// <param name="persistence">The ActiveX persistence mode.</param>
    void ReplacePropertyBytes(PptxActiveXControl target, byte[] bytes, string contentType, string persistence);
    /// <summary>
    /// Replaces binary persistence with an inline ActiveX property bag.
    /// </summary>
    /// <param name="target">The ActiveX control to modify.</param>
    /// <param name="properties">The replacement properties in persistence order.</param>
    void ReplaceProperties(PptxActiveXControl target, List<PptxActiveXProperty> properties);
    /// <summary>
    /// Scans the control relationship, persistence part, and property data for unsafe content.
    /// </summary>
    /// <param name="target">The ActiveX control to inspect.</param>
    /// <returns>The security diagnostics.</returns>
    PptxResult ScanSecurity(PptxActiveXControl target);
}
