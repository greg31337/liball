using System.Collections.Generic;
using System.IO;

namespace pptx;

/// <summary>
/// Provides public operations for PptxTemplateData values.
/// </summary>
public class PptxTemplateDataOperations : IPptxTemplateDataOperations
{
    /// <summary>
    /// Creates an operations instance.
    /// </summary>
    public PptxTemplateDataOperations()
    {
    }

    /// <summary>
    /// Returns the number of items currently available.
    /// </summary>
    /// <returns>The number of matching or affected items.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int Count(PptxTemplateData target)
    {
        return target.Count();
    }

    /// <summary>
    /// Adds an item and returns the created model when applicable.
    /// </summary>
    /// <param name="key">The key value to use.</param>
    /// <param name="value">The value to use.</param>
    /// <param name="target">The data value to operate on.</param>
    public void Add(PptxTemplateData target, string key, string value)
    {
        target.Add(key, value);
    }

    /// <summary>
    /// Reports whether the template data contains the supplied value or coordinates.
    /// </summary>
    /// <param name="key">The key value to use.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    /// <param name="target">The data value to operate on.</param>
    public bool Contains(PptxTemplateData target, string key)
    {
        return target.Contains(key);
    }

    /// <summary>
    /// Returns the value associated with the requested template key.
    /// </summary>
    /// <param name="key">The key value to use.</param>
    /// <returns>The resulting text.</returns>
    /// <param name="target">The data value to operate on.</param>
    public string Get(PptxTemplateData target, string key)
    {
        return target.Get(key);
    }

    /// <summary>
    /// Returns the zero-based index of a template key, or <c>-1</c> when it is absent.
    /// </summary>
    /// <param name="key">The key value to use.</param>
    /// <returns>The resulting numeric value.</returns>
    /// <param name="target">The data value to operate on.</param>
    public int IndexOf(PptxTemplateData target, string key)
    {
        return target.IndexOf(key);
    }

    /// <summary>
    /// Creates template data by copying keys and values from a dictionary.
    /// </summary>
    /// <param name="values">The values value to use.</param>
    /// <returns>The resulting template data value.</returns>
    public PptxTemplateData FromDictionary(Dictionary<string, string> values)
    {
        return PptxTemplateData.FromDictionary(values);
    }

    /// <summary>
    /// Creates template data by pairing keys and values at matching list indexes.
    /// </summary>
    /// <param name="keys">The keys value to use.</param>
    /// <param name="values">The values value to use.</param>
    /// <returns>The resulting template data value.</returns>
    public PptxTemplateData FromLists(List<string> keys, List<string> values)
    {
        return PptxTemplateData.FromLists(keys, values);
    }
}
