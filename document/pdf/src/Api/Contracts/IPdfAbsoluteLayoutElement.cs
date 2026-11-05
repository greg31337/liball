namespace pdf;

/// <summary>
/// Configures clipping for an absolute-positioned layout element.
/// </summary>
public interface IPdfAbsoluteLayoutElement
{
    /// <summary>
    /// Clips child drawing to the configured absolute frame.
    /// </summary>
    void EnableClipping();

    /// <summary>
    /// Allows child drawing to extend beyond the configured absolute frame.
    /// </summary>
    void DisableClipping();
}
