using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxSectionOperations"/>.
/// </summary>
public interface IPptxSectionOperations
{
    /// <summary>
    /// Adds the slide id and returns the created model when applicable.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void AddSlideId(PptxSection target, int slideId);
    /// <summary>
    /// Returns the number of ordered slide references in the section.
    /// </summary>
    /// <param name="target">The section to inspect.</param>
    /// <returns>The number of slide references.</returns>
    int SlideCount(PptxSection target);
    /// <summary>
    /// Returns the slide id at the specified ordered position.
    /// </summary>
    /// <param name="target">The section to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The referenced slide id.</returns>
    int GetSlideId(PptxSection target, int index);
    /// <summary>
    /// Renames the section and persists the change when it belongs to a presentation.
    /// </summary>
    /// <param name="target">The section to update.</param>
    /// <param name="name">The new section name.</param>
    void Rename(PptxSection target, string name);
    /// <summary>
    /// Replaces the ordered slide references after validating attached presentation slide ids.
    /// </summary>
    /// <param name="target">The section to update.</param>
    /// <param name="slideIds">The ordered slide ids.</param>
    void SetSlideIds(PptxSection target, List<int> slideIds);
    /// <summary>
    /// Inserts a validated slide reference at the specified ordered position.
    /// </summary>
    /// <param name="target">The section to update.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="slideId">The referenced slide id.</param>
    void InsertSlideId(PptxSection target, int index, int slideId);
    /// <summary>
    /// Removes the slide reference at the specified ordered position.
    /// </summary>
    /// <param name="target">The section to update.</param>
    /// <param name="index">The zero-based index of the item.</param>
    void RemoveSlideIdAt(PptxSection target, int index);
    /// <summary>
    /// Removes the section from its presentation.
    /// </summary>
    /// <param name="target">The section to remove.</param>
    /// <returns><c>true</c> when the attached section was removed; otherwise, <c>false</c>.</returns>
    bool Remove(PptxSection target);
}
