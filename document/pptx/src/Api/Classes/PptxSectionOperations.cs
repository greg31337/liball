using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxSection values.
/// </summary>
public class PptxSectionOperations : IPptxSectionOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxSectionOperations()
    {
    }

    /// <summary>
    /// Adds the slide id and returns the created model when applicable.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void AddSlideId(PptxSection target, int slideId)
    {
        target.AddSlideId(slideId);
    }
    /// <summary>
    /// Returns the number of ordered slide references in the section.
    /// </summary>
    /// <param name="target">The section to inspect.</param>
    /// <returns>The number of slide references.</returns>
    public int SlideCount(PptxSection target)
    {
        return target.SlideCount();
    }
    /// <summary>
    /// Returns the slide id at the specified ordered position.
    /// </summary>
    /// <param name="target">The section to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The referenced slide id.</returns>
    public int GetSlideId(PptxSection target, int index)
    {
        return target.GetSlideId(index);
    }
    /// <summary>
    /// Renames the section and persists the change when it belongs to a presentation.
    /// </summary>
    /// <param name="target">The section to update.</param>
    /// <param name="name">The new section name.</param>
    public void Rename(PptxSection target, string name)
    {
        target.Rename(name);
    }
    /// <summary>
    /// Replaces the ordered slide references after validating attached presentation slide ids.
    /// </summary>
    /// <param name="target">The section to update.</param>
    /// <param name="slideIds">The ordered slide ids.</param>
    public void SetSlideIds(PptxSection target, List<int> slideIds)
    {
        target.SetSlideIds(slideIds);
    }
    /// <summary>
    /// Inserts a validated slide reference at the specified ordered position.
    /// </summary>
    /// <param name="target">The section to update.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="slideId">The referenced slide id.</param>
    public void InsertSlideId(PptxSection target, int index, int slideId)
    {
        target.InsertSlideId(index, slideId);
    }
    /// <summary>
    /// Removes the slide reference at the specified ordered position.
    /// </summary>
    /// <param name="target">The section to update.</param>
    /// <param name="index">The zero-based index of the item.</param>
    public void RemoveSlideIdAt(PptxSection target, int index)
    {
        target.RemoveSlideIdAt(index);
    }
    /// <summary>
    /// Removes the section from its presentation.
    /// </summary>
    /// <param name="target">The section to remove.</param>
    /// <returns><c>true</c> when the attached section was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(PptxSection target)
    {
        return target.Remove();
    }
}
