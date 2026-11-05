using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxCustomShow values.
/// </summary>
public class PptxCustomShowOperations : IPptxCustomShowOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxCustomShowOperations()
    {
    }

    /// <summary>
    /// Adds the slide id and returns the created model when applicable.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void AddSlideId(PptxCustomShow target, int slideId)
    {
        target.AddSlideId(slideId);
    }
    /// <summary>
    /// Returns the number of ordered slide references in the custom show.
    /// </summary>
    /// <param name="target">The custom show to inspect.</param>
    /// <returns>The number of slide references.</returns>
    public int SlideCount(PptxCustomShow target)
    {
        return target.SlideCount();
    }
    /// <summary>
    /// Returns the slide id at the specified ordered position.
    /// </summary>
    /// <param name="target">The custom show to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The referenced slide id.</returns>
    public int GetSlideId(PptxCustomShow target, int index)
    {
        return target.GetSlideId(index);
    }
    /// <summary>
    /// Renames the custom show and persists the change when it belongs to a presentation.
    /// </summary>
    /// <param name="target">The custom show to update.</param>
    /// <param name="name">The new custom-show name.</param>
    public void Rename(PptxCustomShow target, string name)
    {
        target.Rename(name);
    }
    /// <summary>
    /// Replaces the ordered slide references after validating attached presentation slide ids.
    /// </summary>
    /// <param name="target">The custom show to update.</param>
    /// <param name="slideIds">The ordered slide ids.</param>
    public void SetSlideIds(PptxCustomShow target, List<int> slideIds)
    {
        target.SetSlideIds(slideIds);
    }
    /// <summary>
    /// Inserts a validated slide reference at the specified ordered position.
    /// </summary>
    /// <param name="target">The custom show to update.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="slideId">The referenced slide id.</param>
    public void InsertSlideId(PptxCustomShow target, int index, int slideId)
    {
        target.InsertSlideId(index, slideId);
    }
    /// <summary>
    /// Removes the slide reference at the specified ordered position.
    /// </summary>
    /// <param name="target">The custom show to update.</param>
    /// <param name="index">The zero-based index of the item.</param>
    public void RemoveSlideIdAt(PptxCustomShow target, int index)
    {
        target.RemoveSlideIdAt(index);
    }
    /// <summary>
    /// Removes the custom show from its presentation.
    /// </summary>
    /// <param name="target">The custom show to remove.</param>
    /// <returns><c>true</c> when the attached custom show was removed; otherwise, <c>false</c>.</returns>
    public bool Remove(PptxCustomShow target)
    {
        return target.Remove();
    }
}
