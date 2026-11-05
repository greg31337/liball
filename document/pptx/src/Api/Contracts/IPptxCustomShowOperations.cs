using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxCustomShowOperations"/>.
/// </summary>
public interface IPptxCustomShowOperations
{
    /// <summary>
    /// Adds the slide id and returns the created model when applicable.
    /// </summary>
    /// <param name="slideId">The slide id value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void AddSlideId(PptxCustomShow target, int slideId);
    /// <summary>
    /// Returns the number of ordered slide references in the custom show.
    /// </summary>
    /// <param name="target">The custom show to inspect.</param>
    /// <returns>The number of slide references.</returns>
    int SlideCount(PptxCustomShow target);
    /// <summary>
    /// Returns the slide id at the specified ordered position.
    /// </summary>
    /// <param name="target">The custom show to inspect.</param>
    /// <param name="index">The zero-based index of the item.</param>
    /// <returns>The referenced slide id.</returns>
    int GetSlideId(PptxCustomShow target, int index);
    /// <summary>
    /// Renames the custom show and persists the change when it belongs to a presentation.
    /// </summary>
    /// <param name="target">The custom show to update.</param>
    /// <param name="name">The new custom-show name.</param>
    void Rename(PptxCustomShow target, string name);
    /// <summary>
    /// Replaces the ordered slide references after validating attached presentation slide ids.
    /// </summary>
    /// <param name="target">The custom show to update.</param>
    /// <param name="slideIds">The ordered slide ids.</param>
    void SetSlideIds(PptxCustomShow target, List<int> slideIds);
    /// <summary>
    /// Inserts a validated slide reference at the specified ordered position.
    /// </summary>
    /// <param name="target">The custom show to update.</param>
    /// <param name="index">The zero-based insertion index.</param>
    /// <param name="slideId">The referenced slide id.</param>
    void InsertSlideId(PptxCustomShow target, int index, int slideId);
    /// <summary>
    /// Removes the slide reference at the specified ordered position.
    /// </summary>
    /// <param name="target">The custom show to update.</param>
    /// <param name="index">The zero-based index of the item.</param>
    void RemoveSlideIdAt(PptxCustomShow target, int index);
    /// <summary>
    /// Removes the custom show from its presentation.
    /// </summary>
    /// <param name="target">The custom show to remove.</param>
    /// <returns><c>true</c> when the attached custom show was removed; otherwise, <c>false</c>.</returns>
    bool Remove(PptxCustomShow target);
}
