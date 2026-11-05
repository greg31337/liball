using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxHyperlinkOperations"/>.
/// </summary>
public interface IPptxHyperlinkOperations
{
    /// <summary>
    /// Reports whether the hyperlink contains or enables link.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool HasLink(PptxHyperlink target);
    /// <summary>
    /// Sets an external URL on the hyperlink owner.
    /// </summary>
    /// <param name="url">The external URL.</param>
    /// <param name="target">The hyperlink to update.</param>
    void SetUrl(PptxHyperlink target, string url);
    /// <summary>
    /// Sets an external file URI on the hyperlink owner.
    /// </summary>
    /// <param name="fileUri">The external file URI.</param>
    /// <param name="target">The hyperlink to update.</param>
    void SetFile(PptxHyperlink target, string fileUri);
    /// <summary>
    /// Links the hyperlink owner to an internal slide.
    /// </summary>
    /// <param name="slideIndex">The zero-based target slide index.</param>
    /// <param name="target">The hyperlink to update.</param>
    void SetSlide(PptxHyperlink target, int slideIndex);
    /// <summary>
    /// Sets a next, previous, first, or last slide navigation action.
    /// </summary>
    /// <param name="navigation">The navigation type.</param>
    /// <param name="target">The hyperlink to update.</param>
    void SetNavigation(PptxHyperlink target, string navigation);
    /// <summary>
    /// Sets a custom-show action.
    /// </summary>
    /// <param name="customShowName">The custom show name.</param>
    /// <param name="target">The hyperlink to update.</param>
    void SetCustomShow(PptxHyperlink target, string customShowName);
    /// <summary>
    /// Sets a raw preserved PowerPoint action, including macro actions.
    /// </summary>
    /// <param name="action">The complete action URI.</param>
    /// <param name="target">The hyperlink to update.</param>
    void SetAction(PptxHyperlink target, string action);
    /// <summary>
    /// Removes the hyperlink element and its relationship.
    /// </summary>
    /// <param name="target">The hyperlink to remove.</param>
    void Remove(PptxHyperlink target);
    /// <summary>
    /// Audits the hyperlink target or action without changing it.
    /// </summary>
    /// <returns>Warnings for file, unsafe-scheme, macro, program, unknown, or broken links.</returns>
    /// <param name="target">The hyperlink to audit.</param>
    PptxResult AuditSafety(PptxHyperlink target);
}
