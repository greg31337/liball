using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Defines the operations exposed by <see cref="PptxTemplateDataOperations"/>.
/// </summary>
public interface IPptxTemplateDataOperations
{
    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    int Count(PptxTemplateData target);
    /// <summary>
    /// Adds an item and returns the created model when applicable.
    /// </summary>
    /// <param name="key">The key value to use.</param>
    /// <param name="value">The value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    void Add(PptxTemplateData target, string key, string value);
    /// <summary>
    /// Reports whether the template data contains the supplied value or coordinates.
    /// </summary>
    /// <param name="key">The key value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    bool Contains(PptxTemplateData target, string key);
    /// <summary>
    /// Returns the value associated with the requested template key.
    /// </summary>
    /// <param name="key">The key value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    string Get(PptxTemplateData target, string key);
    /// <summary>
    /// Returns the zero-based index of a template key, or <c>-1</c> when it is absent.
    /// </summary>
    /// <param name="key">The key value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    int IndexOf(PptxTemplateData target, string key);
    /// <summary>
    /// Creates template data by copying keys and values from a dictionary.
    /// </summary>
    /// <param name="values">The values value to use.</param>
    /// <returns>The resulting template data value.</returns>
    PptxTemplateData FromDictionary(Dictionary<string, string> values);
    /// <summary>
    /// Creates template data by pairing keys and values at matching list indexes.
    /// </summary>
    /// <param name="keys">The keys value to use.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The resulting template data value.</returns>
    PptxTemplateData FromLists(List<string> keys, List<string> values);
}
